export function updateOrAddElement<A>(
	array: A[],
	p: (a: A) => boolean,
	newEntry: A
) {
	if (array.find(p)) {
		return array.map((e) => (!p(e) ? e : newEntry));
	}
	return [...array, newEntry];
}

export function isNullOrEmpty(array: any[] | undefined | null) {
	return !array || array.length === 0;
}

export function setIncluded<A>(array: A[], elem: A, included: boolean): A[] {
	const already = array.includes(elem);
	if (included === already) {
		return array;
	}
	if (included) {
		return [...array, elem];
	}
	return array.filter((e) => e !== elem);
}

export function notUndefined<A>(a: any): a is A {
	return a !== undefined;
}

export function compareAsSet(a: any[], b: any[]) {
	return (
		a === b ||
		(a && b && a.length === b.length && a.every((e) => b.includes(e)))
	);
}
